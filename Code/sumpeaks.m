%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%   CSEE 6893: Big Data Analytics
%   Bridge Health Monitoring
%   Author: Karl S. Bayer (karlsbayer at gmail dot com)
%   Created: 12/2015
  
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
 

function [ summedpeaks ] = sumpeaks( ppeaks, llocs )
%UNTITLED6 Summary of this function goes here
%   Detailed explanation goes here
X = [llocs.', ppeaks.'];
[a,~,c] = unique(X(:,1));
summedpeaks = [a, accumarray(c,X(:,2))];

end

